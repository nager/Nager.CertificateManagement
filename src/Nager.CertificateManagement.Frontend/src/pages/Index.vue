<template>
  <q-page padding>
    <q-card class="AddCertificateJob q-mb-md">
      <q-card-section>
        <q-input
          v-model="fqdn"
          dense
          class="q-mb-sm"
          label="Fully Qualified Domain Name (e.g. subdomain.mydomain.com)"
        />

        <q-btn
          color="primary"
          label="Add certificate job"
          @click="create"
        />
      </q-card-section>
    </q-card>

    <q-table
      title="Certificate Jobs"
      dense
      :data="certificateJobs"
      :columns="columns"
      row-key="id"
    >
      <template v-slot:body-cell-actions="props">
        <q-td :props="props">
          <q-btn
            dense
            round
            flat
            color="grey"
            icon="cloud_download"
            type="a"
            :href="`api/certificatejob/download/${props.row.id}`"
          />
        </q-td>
      </template>
    </q-table>
  </q-page>
</template>

<script>
export default {
  name: 'PageIndex',
  data () {
    return {
      columns: [
        { name: 'fqdn', align: 'left', label: 'FQDN', field: 'fqdn' },
        { name: 'created', align: 'left', label: 'Created', field: 'created' },
        { name: 'updated', align: 'left', label: 'Updated', field: 'updated' },
        { name: 'status', align: 'left', label: 'Status', field: 'status' },
        { name: 'actions', align: 'left', label: 'Actions' }
      ],
      fqdn: '',
      certificateJobs: []
    }
  },
  async created () {
    await this.getAll()
  },
  methods: {
    async getAll () {
      try {
        var response = await this.$axios.get('/api/CertificateJob')
        this.certificateJobs = response.data
      } catch (error) {
        this.$q.notify({
          type: 'negative',
          message: 'Request failure',
          caption: `${error}`
        })
      }
    },
    async create () {
      try {
        await this.$axios.post('/api/CertificateJob', { fqdn: this.fqdn })
        await this.getAll()
      } catch (error) {
        this.$q.notify({
          type: 'negative',
          message: 'Request failure',
          caption: `${error}`
        })
      }
    }
  }
}
</script>
